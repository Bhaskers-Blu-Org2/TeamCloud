# coding=utf-8
# --------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
#
# Code generated by Microsoft (R) AutoRest Code Generator.
# Changes may cause incorrect behavior and will be lost if the code is
# regenerated.
# --------------------------------------------------------------------------

from msrest.serialization import Model


class ProjectUserListDataResult(Model):
    """ProjectUserListDataResult.

    Variables are only populated by the server, and will be ignored when
    sending a request.

    :param code:
    :type code: int
    :param status:
    :type status: str
    :ivar data:
    :vartype data: list[~teamcloud.models.ProjectUser]
    :param location:
    :type location: str
    """

    _validation = {
        'data': {'readonly': True},
    }

    _attribute_map = {
        'code': {'key': 'code', 'type': 'int'},
        'status': {'key': 'status', 'type': 'str'},
        'data': {'key': 'data', 'type': '[ProjectUser]'},
        'location': {'key': 'location', 'type': 'str'},
    }

    def __init__(self, **kwargs):
        super(ProjectUserListDataResult, self).__init__(**kwargs)
        self.code = kwargs.get('code', None)
        self.status = kwargs.get('status', None)
        self.data = None
        self.location = kwargs.get('location', None)
